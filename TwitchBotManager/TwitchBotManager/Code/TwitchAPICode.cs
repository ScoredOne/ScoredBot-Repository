using System.Collections.Generic;
using System.Threading.Tasks;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.V5.Models.Subscriptions;

namespace TwitchBotManager.Code {
	class TwitchAPICode {
        private static TwitchAPI api;

        public TwitchAPICode() {
            api = new TwitchAPI();
            api.Settings.ClientId = "client_id";
            api.Settings.AccessToken = "access_token"; // App Secret is not an Accesstoken
        }

        private async Task ExampleCallsAsync() {
            //Checks subscription for a specific user and the channel specified.
            Subscription subscription = await api.V5.Channels.CheckChannelSubscriptionByUserAsync("channel_id", "user_id");

            //Gets a list of all the subscritions of the specified channel.
            List<Subscription> allSubscriptions = await api.V5.Channels.GetAllSubscribersAsync("channel_id");

            //Get channels a specified user follows.
            GetUsersFollowsResponse userFollows = await api.Helix.Users.GetUsersFollowsAsync("user_id");

            //Get Specified Channel Follows
            var channelFollowers = await api.V5.Channels.GetChannelFollowersAsync("channel_id");

            //Return bool if channel is online/offline.
            bool isStreaming = await api.V5.Streams.BroadcasterOnlineAsync("channel_id");

            //Update Channel Title/Game
            await api.V5.Channels.UpdateChannelAsync("channel_id", "New stream title", "Stronghold Crusader");
        }
    }
}
