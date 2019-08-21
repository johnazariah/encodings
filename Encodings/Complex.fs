namespace Encodings
[<AutoOpen>]
module Complex =
    open System
    let (=?) (l : Double) (r : Double) =
        if Double.IsNaN l || Double.IsNaN r then
            Double.IsNaN r && Double.IsNaN l
        else
            let epsilon = 1E-10
            Math.Abs (l - r) <= epsilon

    type Polar = {
        Amplitude : double
        PhaseAngle : double
    } with
        member this.ToCartesian = {
            Re = this.Amplitude * Math.Cos(this.PhaseAngle)
            Im = this.Amplitude * Math.Sin(this.PhaseAngle)
        }

    and [<CustomEquality>][<NoComparison>] Complex = {
        Re : double
        Im : double
    } with
        member this.IsIncomputable =
            let hasNaNComponent =
                Double.IsNaN this.Re || Double.IsNaN this.Im
            let hasInfinityComponent=
                Double.IsInfinity this.Re || Double.IsInfinity this.Im
            let isTooLarge =
                Math.Abs(this.Re) >= 1E308 || Math.Abs(this.Im) >= 1E308
            hasNaNComponent || hasInfinityComponent || isTooLarge

        static member Zero =
            { Re = 0.0; Im = 0.0 }
        static member Unity =
            { Re = 1.0; Im = 0.0 }
        static member (+) (l, r) =
            { Re = (l.Re + r.Re); Im = (l.Im + r.Im) }
        static member (-) (l, r) =
            { Re = (l.Re - r.Re); Im = (l.Im - r.Im) }
        static member (~-) (l) =
            { Re = -l.Re; Im = -l.Im }
        static member (*) (l, r) =
            { Re = (l.Re * r.Re) - (l.Im * r.Im); Im = (l.Re * r.Im + l.Im * r.Re) }
        static member (~~) (v) =
            { Re = v.Re; Im = -v.Im }

        member this.TimesI =
            { Re = -this.Im; Im = this.Re }

        override this.ToString() =
            let sign = if this.Im < 0.0 then "-" else "+"
            let im = if this.Im = 0.0 then "" else sprintf " %s %.4f i" sign (Math.Abs this.Im)
            sprintf "(%.4f%s)" (this.Re) im

        override this.Equals(other) =
            let approxEquals (l : Complex, r : Complex) =
                if l.IsIncomputable then
                    r.IsIncomputable
                else if r.IsIncomputable then
                    l.IsIncomputable
                else
                    (l.Re =? r.Re)
            in
            match other with
            | :? Complex as o -> approxEquals (this, o)
            | _ -> false

        override this.GetHashCode() =
            this.Re.GetHashCode() ^^^ this.Im.GetHashCode()

        member this.ToPolar =
            let amplitude  =
                Math.Sqrt(this.Re * this.Re + this.Im * this.Im)
            let phaseAngle =
                if this.Re = 0.0 then
                    Math.Atan(Double.PositiveInfinity)
                else
                    Math.Atan2(this.Im, this.Re)
            { Amplitude = amplitude; PhaseAngle = phaseAngle }
