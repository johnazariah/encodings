[<AutoOpen>]
module TestUtils
    open FsCheck
    open Encodings
    open System.Numerics

    type TestcaseDatabase<'a> (generator : 'a[] seq) =
        interface seq<'a[]> with
            member __.GetEnumerator() =
                generator.GetEnumerator()
            member __.GetEnumerator() =
                generator.GetEnumerator() :> System.Collections.IEnumerator

    type ComplexGenerator =
        static member NormalComplex() =
            { new Arbitrary<System.Numerics.Complex>() with
                override __.Shrinker t = Seq.empty
                override __.Generator =
                    gen {
                        let! re = Arb.Default.NormalFloat().Generator
                        let! im = Arb.Default.NormalFloat().Generator
                        return System.Numerics.Complex (re.Get, im.Get)
                    }
            }


    type CChar =
    | CC of char
    with
        member this.Unapply = match this with CC c -> c
        member this.Signature = this.ToString()
        static member (<.>) (l : C<CChar>, r : C<CChar>) : C<C<CChar>[]> =
            C<_>.Apply (Complex.One, [| l; r |])


    type Wick =
    | Raise
    | Lower
    with
        static member FromString =
            function
            | "R" -> Some Raise
            | "L" -> Some Lower
            | _   -> None
        override this.ToString() =
            match this with
            | Raise -> "R"
            | Lower -> "L"
        static member InNormalOrder (l, r) =
            match (l, r) with
            | Lower, Raise -> false
            | _, _ -> true
