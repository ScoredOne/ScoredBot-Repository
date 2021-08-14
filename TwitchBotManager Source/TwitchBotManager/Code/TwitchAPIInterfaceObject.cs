using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.ThirdParty.AuthorizationFlow;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Subscriptions;

namespace TwitchBotManager.Code {
	class TwitchAPIInterfaceObject {
        private static TwitchAPI API;

        private IEnumerable<AuthScopes> authScopes = new List<AuthScopes>() { AuthScopes.Chat_Login, AuthScopes.User_Read, AuthScopes.Channel_Read };

        public TwitchAPIInterfaceObject(string OAuth, string secret) {
            API = new TwitchAPI();
            API.Settings.ClientId = OAuth; // This is confusing and the naming needs sorting
            API.Settings.Secret = secret;
			//api.Settings.AccessToken = "access_token"; // App Secret is not an Accesstoken

			API.ThirdParty.AuthorizationFlow.OnUserAuthorizationDetected += AuthorizationFlow_OnUserAuthorizationDetected;
			API.ThirdParty.AuthorizationFlow.OnError += AuthorizationFlow_OnError;
        }

		private void AuthorizationFlow_OnUserAuthorizationDetected(object sender, TwitchLib.Api.Events.OnUserAuthorizationDetectedArgs e) {
            MainForm.StaticPostToDebug("Authorization detected! Below are the details:");
            MainForm.StaticPostToDebug($"Flow Id: {e.Id}");
            MainForm.StaticPostToDebug($"Username: {e.Username}");
            MainForm.StaticPostToDebug($"Token: {e.Token}");
            MainForm.StaticPostToDebug($"Refresh: {e.Refresh}");
            MainForm.StaticPostToDebug($"Scopes: {string.Join(", ", e.Scopes)}");

            Console.WriteLine();
            MainForm.StaticPostToDebug("Verifying refresh functionality...");

            RefreshTokenResponse resp = API.ThirdParty.AuthorizationFlow.RefreshToken(e.Refresh);

            MainForm.StaticPostToDebug($"Refreshed token: {resp.Token}");
            MainForm.StaticPostToDebug($"Refreshed refresh token: {resp.Refresh}");
        }

		private void AuthorizationFlow_OnError(object sender, TwitchLib.Api.Events.OnAuthorizationFlowErrorArgs e) {
            MainForm.StaticPostToDebug($"[TwitchAPIInterfaceObject - ERROR - {e.Error}] {e.Message}");
        }

		public void InitialiseAccess() {
            // Confirmed working, just need to clean this up and where to use it before removing

            //CreatedFlow createdFlow = API.ThirdParty.AuthorizationFlow.CreateFlow("ScoredBot TwitchBot", authScopes);

            //Process process = Process.Start(createdFlow.Url);

            //API.ThirdParty.AuthorizationFlow.BeginPingingStatus(createdFlow.Id, 5000);
		}

        //private async Task ExampleCallsAsync() {
        //    //Checks subscription for a specific user and the channel specified.
        //    Subscription subscription = await API.V5.Channels.CheckChannelSubscriptionByUserAsync("channel_id", "user_id");

        //    //Gets a list of all the subscritions of the specified channel.
        //    List<Subscription> allSubscriptions = await API.V5.Channels.GetAllSubscribersAsync("channel_id");

        //    //Get channels a specified user follows.
        //    GetUsersFollowsResponse userFollows = await API.Helix.Users.GetUsersFollowsAsync("user_id");

        //    //Get Specified Channel Follows
        //    ChannelFollowers channelFollowers = await API.V5.Channels.GetChannelFollowersAsync("channel_id");

        //    //Return bool if channel is online/offline.
        //    bool isStreaming = await API.V5.Streams.BroadcasterOnlineAsync("channel_id");

        //    //Update Channel Title/Game
        //    await API.V5.Channels.UpdateChannelAsync("channel_id", "New stream title", "Stronghold Crusader");
        //}
    }
}
