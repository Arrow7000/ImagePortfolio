module Api

open FSharp.Json


let config = JsonConfig.create(jsonFieldNaming = Json.lowerCamelCase)
let serialise o = Json.serializeEx config o
