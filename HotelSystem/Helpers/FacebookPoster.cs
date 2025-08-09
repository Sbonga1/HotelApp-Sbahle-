using System;
using System.Threading.Tasks;
using Facebook;

namespace HotelSystem.Helpers
{
    public static class FacebookPoster
    {
        public static async Task<string> PostEventAsync(string pageAccessToken, string pageId, string message, string link = null)
        {
            try
            {
                var client = new FacebookClient(pageAccessToken);

                dynamic parameters = new
                {
                    message = message,
                };

                var result = await Task.Run(() => client.Post($"{pageId}/feed", parameters));
                return result.id;
            }
            catch (FacebookApiException ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
