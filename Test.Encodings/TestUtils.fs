﻿[<AutoOpen>]
module TestUtils
    open FsCheck

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