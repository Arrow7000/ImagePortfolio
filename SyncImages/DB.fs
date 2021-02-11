module DB

open FSharp.Data.GraphQL
open FSharp.Data.LiteralProviders
open System
open ImageConversion

[<Literal>]
let adminSecret = Env.HASURA_GRAPHQL_ADMIN_SECRET.Value

[<Literal>]
let headerKey = "x-hasura-admin-secret"
[<Literal>]
let headers = headerKey + ":" + adminSecret

type GraphQlClient = GraphQLProvider<"https://composed-glider-47.hasura.app/v1/graphql", httpHeaders=headers>




let getOptOrFailwithErrs dataOpt errs =
    try
        Option.get dataOpt
    with _ ->
        failwithf "%A" errs




type Photo =
    { Id: Guid
      PhotoHash: OrigImgHash
      OrderIndex: int option
      Slug: string
      Title: string
      Description: string 
      Width: int
      Height: int }




// @TODO: mprobably need to show height and width instead of just size of the longest side
type SizedImage =
    { Width     : int // won't include originals
      ImageUrl  : ImageUrl }



type FullPhoto =
    { Photo : Photo
      Sizes : SizedImage list }


let makeFullPhoto sizes (photo : Photo) =
    let sizedImages =
        sizes
        |> List.map
            (fun size ->
                let w,_ = scaleMaxTo size (photo.Width,photo.Height)
                { Width = w; ImageUrl = getImageUrl photo.PhotoHash (Size size) })

    { Photo = photo
      Sizes = sizedImages }




let addNewPhotoMutation =
    GraphQlClient.Operation<"""
        fragment singlePhoto on photos {
            id
            orderindex
            origImageHash
            slug
            title
            description
            height
            width
        }

        mutation AddPhoto($id: uuid!, $hash: String!, $height: Int!, $width: Int!, $slug: String, $title: String!, $description: String!) {
          insert_photos_one(object: {id: $id, origImageHash: $hash, height: $height, width: $width, slug: $slug, title: $title, description: $description, ispublished: true}) {
            ... singlePhoto
          }
        }
    """>
        ()



let addNewPhotoToDb (id: Guid) (OrigImgHash hash) (height:int, width:int) (slug: string) (title: string) (description: string) =
    async {
        let! result =
            addNewPhotoMutation
                .AsyncRun(id=id.ToString(), hash=hash, height=height, width=width, slug=slug, title=title, description=description)
        let data =
            getOptOrFailwithErrs result.Data result.Errors
        let photo = data.Insert_photos_one |> Option.get

        return
            { Id = Guid.Parse photo.Id
              PhotoHash = OrigImgHash hash
              OrderIndex = photo.Orderindex
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description 
              Height = photo.Height
              Width = photo.Width }
    }


let getAllSizesQuery =
    GraphQlClient.Operation<"""
        query GetAllSizes {
          sizes {
            size
          }
        }
    """>
        ()


let getSizes () =
    async {
        let! result = getAllSizesQuery.AsyncRun()
        let data = Option.get result.Data

        return seq { for size in data.Sizes -> size.Size } |> List.ofSeq
    }





let getSinglePhotoQuery =
    GraphQlClient.Operation<"""
        query GetSinglePhoto($id: uuid!) {
          photos_by_pk(id: $id) {
            id
            orderindex
            origImageHash
            slug
            title
            description
            height
            width
          }
        }
    """>
        ()

let getSinglePhoto id =
    async {
        let! result = getSinglePhotoQuery.AsyncRun(id)
        let data = getOptOrFailwithErrs result.Data result.Errors
        let photo = Option.get data.Photos_by_pk

        return
            { Id = Guid.Parse photo.Id
              PhotoHash = OrigImgHash photo.OrigImageHash
              OrderIndex = photo.Orderindex
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description
              Height = photo.Height
              Width = photo.Width }
    }





let getSinglePhotoBySlugQuery =
    GraphQlClient.Operation<"""
        query GetSinglePhotoBySlug($slug: String!) {
          photos(where: {slug: {_eq: $slug}}) {
            id
            description
            orderindex
            origImageHash
            slug
            title
            height
            width
          }
        }
    """>
        ()


let getSinglePhotoBySlug slug =
    async {
        let! result = getSinglePhotoBySlugQuery.AsyncRun(slug)
        let data = getOptOrFailwithErrs result.Data result.Errors
        match data.Photos |> List.ofArray with
        | [] -> return Error "No match for this slug"
        | photo :: _ ->
            return
                { Id = Guid.Parse photo.Id
                  PhotoHash = OrigImgHash photo.OrigImageHash
                  OrderIndex = photo.Orderindex
                  Slug = photo.Slug
                  Title = photo.Title
                  Description = photo.Description
                  Height = photo.Height
                  Width = photo.Width }
                |> Ok
    }




let getAllPhotosQuery =
    GraphQlClient.Operation<"""
        query GetAllPhotos {
            photos {
              id
              orderindex
              origImageHash
              title
              slug
              description
              height
              width
            }
        }
    """>
        ()



let getAllPhotos () =
    async {
        let! result = getAllPhotosQuery.AsyncRun()
        let data = getOptOrFailwithErrs result.Data result.Errors

        return
            seq {
                for photo in data.Photos ->
                    { Id = Guid.Parse photo.Id
                      PhotoHash = OrigImgHash photo.OrigImageHash
                      OrderIndex = photo.Orderindex
                      Slug = photo.Slug
                      Title = photo.Title
                      Description = photo.Description
                      Height = photo.Height
                      Width = photo.Width }
            }
            |> List.ofSeq
    }



let editPhotoMut =
    GraphQlClient.Operation<"""
        mutation EditPhoto($id: uuid!, $obj: photos_set_input) {
          update_photos_by_pk(pk_columns: {id: $id}, _set: $obj) {
            id
            orderindex
            origImageHash
            slug
            title
            description
            height
            width
          }
        }
    """>()


type PhotoEditInput = GraphQlClient.Types.Photos_set_input

/// Defaults an option to a fallback value
let private deflt o d = Option.defaultValue d o


let changePhotoFields id (titleOpt : string option) (slugOpt : string option) (descrOpt : string option) (hashDimsOpt: (OrigImgHash * OrigDimensions) option) =
    async {
        let! photo = getSinglePhoto id
        let (OrigImgHash currHashStr) = photo.PhotoHash

        let hashStrOpt, dimsOpt =
            match hashDimsOpt with
            | Some (OrigImgHash hash, dims) -> Some hash, Some dims
            | None -> None, None

        let height =
            dimsOpt
            |> Option.map (fun dim -> dim.Height)
            |> Option.defaultValue photo.Height

        let width =
            dimsOpt
            |> Option.map (fun dim -> dim.Width)
            |> Option.defaultValue photo.Width

        let input =
            new PhotoEditInput( id=id,
                                title=deflt titleOpt photo.Title,
                                slug=deflt slugOpt photo.Slug,
                                description=deflt descrOpt photo.Description,
                                origImageHash=deflt hashStrOpt currHashStr,
                                height=height,
                                width=width)

        let! result = editPhotoMut.AsyncRun(id=id, obj=input)
        let data = getOptOrFailwithErrs result.Data result.Errors
        let photo = Option.get data.Update_photos_by_pk
        return
            { Id = Guid.Parse photo.Id
              PhotoHash = OrigImgHash photo.OrigImageHash
              OrderIndex = photo.Orderindex
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description
              Height = photo.Height
              Width = photo.Width }
    }

let private reorderPhoto id index =
    async {
        let input = new PhotoEditInput(id=id, orderindex=index)
        let! result = editPhotoMut.AsyncRun(id=id, obj=input)
        let data = getOptOrFailwithErrs result.Data result.Errors
        let photo = Option.get data.Update_photos_by_pk
        return
            { Id = Guid.Parse photo.Id
              PhotoHash = OrigImgHash photo.OrigImageHash
              OrderIndex = photo.Orderindex
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description
              Height = photo.Height
              Width = photo.Width }
    }


let reorderPhotos orderedIds =
    async {
        let! results =
            orderedIds
            |> List.mapi (fun index id -> reorderPhoto id index)
            |> Async.Parallel
            |> Async.map Array.toList

        return results
    }


let deletePhotoMut =
    GraphQlClient.Operation<"""
        mutation DeletePhoto($id: uuid!) {
          delete_photos_by_pk(id: $id) {
            id
          }
        }
    """>()


let deletePhoto id =
    async {
        let! result = deletePhotoMut.AsyncRun(id)
        let data = getOptOrFailwithErrs result.Data result.Errors
        let photo = Option.get data.Delete_photos_by_pk
        return photo.Id
    }
