using CardValidationApp;

namespace SimilarRest.Models
{
    public class CardInfoResponse
    {
        public string Id { get; set; }
        public CardInfo CardInfo { get; set; }
        public string Status { get; set; }
    }
}
