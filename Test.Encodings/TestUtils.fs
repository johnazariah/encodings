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

        static member Apply (coeff, thunk) = (coeff, thunk) |> (C<_>.Apply >> CC)
        static member New thunk = CChar.Apply (Complex.One, thunk)

        member this.IsIdentity = false
        static member (<*>) (l, r)     = failwith "NYI"

        static member InIndexOrder    (a : IxOp<uint32, CChar>, b : IxOp<uint32, CChar>) = a.Index >= b.Index
        static member InOperatorOrder (a : IxOp<uint32, CChar>, b : IxOp<uint32, CChar>) = a.Op    >  b.Op
        static member ToOperatorOrder (a : IxOp<uint32, CChar>, b : IxOp<uint32, CChar>) : C<IxOp<uint32, CChar>[]>[] = failwith "FYI"
        static member ToIndexOrder    (a : IxOp<uint32, CChar>, b : IxOp<uint32, CChar>) : C<IxOp<uint32, CChar>[]>[] = failwith "FYI"
