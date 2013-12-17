module Helpers

let castAs<'T when 'T : null> (o:obj) = 
  match o with
  | :? 'T as res -> res
  | _ -> null

let isType<'T> o =
    match box o with 
    | :? 'T -> true
    | _ -> false