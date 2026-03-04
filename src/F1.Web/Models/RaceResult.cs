namespace F1.Web.Models
{
    public class RaceResult
    {
        public string? DriverId { get; set; }
        public int Position { get; set; }
        public int Points { get; set; }

        public string PositionColor
        {
            get
            {
                return Position switch
                {
                    1 => "gold",
                    2 => "silver",
                    3 => "#cd7f32",
                    _ => "transparent",
                };
            }
        }
    }
}
