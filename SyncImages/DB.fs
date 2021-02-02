module DB

open FSharp.Data.GraphQL

type GraphQlClient = GraphQLProvider<"https://composed-glider-47.hasura.app/v1/graphql">




let getOptOrFailwithErrs dataOpt errs =
    try
        Option.get dataOpt
    with _ ->
        failwithf "%A" errs



type Photo =
    { Id: string
      PhotoHash: OrigImgHash
      Slug: string
      Title: string
      Description: string }



type SizedImage =
    { Size      : int // won't include originals
      ImageUrl  : ImageUrl }



type FullPhoto =
    { Photo : Photo
      Sizes : SizedImage list }


let makeFullPhoto sizes (photo : Photo) =
    let sizedImages =
        sizes
        |> List.map
            (fun size -> { Size = size; ImageUrl = getImageUrl photo.PhotoHash (Size size) })

    { Photo = photo
      Sizes = sizedImages }





let addNewPhotoMutation =
    GraphQlClient.Operation<"""
        mutation AddPhoto($id: uuid!, $hash: String!, $slug: String, $title: String!, $description: String!) {
          insert_photos_one(object: {id: $id, origImageHash: $hash, slug: $slug, title: $title, description: $description}) {
            id
            title
            slug
            description
          }
        }
    """>
        ()



let addNewPhotoToDb (id: string) (OrigImgHash hash) (slug: string) (title: string) (description: string) =
    async {
        let! result = addNewPhotoMutation.AsyncRun(id, hash, slug, title, description)
        let data =
            getOptOrFailwithErrs result.Data result.Errors
        let photo = data.Insert_photos_one |> Option.get

        return
            { Id = photo.Id
              PhotoHash = OrigImgHash hash
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description }
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
            description
            id
            origImageHash
            slug
            title
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
            { Id = photo.Id
              PhotoHash = OrigImgHash photo.OrigImageHash
              Slug = photo.Slug
              Title = photo.Title
              Description = photo.Description }
    }




let getAllPhotosQuery =
    GraphQlClient.Operation<"""
        query GetAllPhotos {
            photos {
              id
              origImageHash
              title
              slug
              description
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
                    { Id = photo.Id
                      PhotoHash = OrigImgHash photo.OrigImageHash
                      Slug = photo.Slug
                      Title = photo.Title
                      Description = photo.Description }
            }
            |> List.ofSeq
    }
