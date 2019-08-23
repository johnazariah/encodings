[<AutoOpen>]
module TestUtils

type TestcaseDatabase<'a> (generator : 'a[] seq) =
    interface seq<'a[]> with
        member this.GetEnumerator() =
            generator.GetEnumerator()
        member this.GetEnumerator() =
            generator.GetEnumerator() :> System.Collections.IEnumerator
