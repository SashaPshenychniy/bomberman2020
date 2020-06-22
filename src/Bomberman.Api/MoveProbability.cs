namespace Bomberman.Api
{
    public struct MoveProbability
    {
        public double KeepDirection { get; set; }
        public double TurnLeft { get; set; }
        public double TurnRight { get; set; }
        public double Stop { get; set; }
        public double Reverse { get; set; }

        public MoveProbability Normalize(bool keepDirectionAllowed, bool turnLeftAllowed, bool turnRightAllowed)
        {
            var sum = KeepDirection * (keepDirectionAllowed ? 1 : 0) +
                      TurnLeft * turnLeftAllowed.AsInt() +
                      TurnRight * turnRightAllowed.AsInt() +
                      Stop + 
                      Reverse;

            var res = new MoveProbability
            {
                KeepDirection = KeepDirection * keepDirectionAllowed.AsInt() / sum,
                TurnLeft = TurnLeft * turnLeftAllowed.AsInt() / sum,
                TurnRight = TurnRight * turnRightAllowed.AsInt() / sum,
                Stop = Stop / sum,
                Reverse = Reverse / sum
            };

            return res;
        }
    }
}