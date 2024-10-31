using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using SimilarRest.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CardValidationApp
{
    class Program
    {
        private const string Host = "https://acstopay.online";
        private const string KeyId = "47e8fde35b164e888a57b6ff27ec020f";
        private const string SharedKey = "ac/1LUdrbivclAeP67iDKX2gPTTNmP0DQdF+0LBcPE/3NWwUqm62u5g6u+GE8uev5w/VMowYXN8ZM+gWPdOuzg==";

        static async Task Main(string[] args)
        {
            var cardNumbers = new[] { "4111111111111111", "4627100101654724", "4486441729154030", "4024007123874108" };
            foreach (var cardNumber in cardNumbers)
            {
                bool result = await ValidateCardAsync(cardNumber);
                Console.WriteLine(result ? "Successfully" : "Unsuccessfully");
            }
        }

        private static async Task<bool> ValidateCardAsync(string pan)
        {
            using HttpClient client = new();
            client.BaseAddress = new Uri(Host);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("kid", KeyId);

            CardInfoRequest cardInfo = new()
            {
                CardInfo = new CardInfo
                {
                    Pan = pan
                }
            };
            string jsonPayload = JsonConvert.SerializeObject(cardInfo);
            string jwsMessage = CreateJwsMessage(jsonPayload);

            StringContent content = new StringContent(jwsMessage, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/testassignments/pan", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return DecodeJwsMessage(responseContent).Status == "Success";
            }
            return false;
        }

        private static string CreateJwsMessage(string payload)
        {
            ProtectedHeader protectedHeader = new ()
            {
                Alg = "HS256",
                Kid = KeyId,
                Signdate = DateTime.UtcNow.ToString("o"),
                Cty = "application/json"
            };

            JsonSerializerSettings jsonSerializerSettings = new ()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };

            string protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader, jsonSerializerSettings);
            string protectedHeaderBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(protectedHeaderJson));
            string payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));

            string signingInput = $"{protectedHeaderBase64}.{payloadBase64}";
            string signature = SignPayload(signingInput);

            return $"{signingInput}.{signature}";
        }

        private static string SignPayload(string signingInput)
        {
            using HMACSHA256 hmac = new(Convert.FromBase64String(SharedKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static CardInfoResponse DecodeJwsMessage(string jwsMessage)
        {
            string[] parts = jwsMessage.Split('.');
            if (parts.Length != 3)
            {
                throw new InvalidOperationException("Invalid JWS format");
            }

            string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            return JsonConvert.DeserializeObject<CardInfoResponse>(payloadJson);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string paddedInput = input.Replace('-', '+').Replace('_', '/');
            switch (paddedInput.Length % 4)
            {
                case 2: paddedInput += "=="; break;
                case 3: paddedInput += "="; break;
            }
            return Convert.FromBase64String(paddedInput);
        }
    }
}
