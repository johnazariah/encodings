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
    | CC of C<char>
    with
        member this.Unapply            = match this with CC c -> c
        member this.Coeff              = this.Unapply.Coeff
        member this.Signature          = this.Unapply.Thunk.ToString()
        member this.IsZero             = this.Unapply.IsZero
        member this.ScaleCoefficient c = this.Unapply.ScaleCoefficient c |> CC
        member this.AddCoefficient   c = this.Unapply.AddCoefficient   c |> CC
        static member (<*>) (l, r)     = failwith "NYI"
        static member Apply (coeff, thunk) = CC <| C<_>.Apply (coeff, thunk)
        static member New thunk = CChar.Apply (Complex.One, thunk)

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
