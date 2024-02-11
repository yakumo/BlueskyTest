using Bluesky.Net;
using Bluesky.Net.Commands.AtProto.Server;
using Bluesky.Net.Commands.Bsky.Feed;
using Bluesky.Net.Models;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueskyTest.ViewModels
{
    public class MainWindowModel
    {
        public ReactiveProperty<string> UserName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> Password { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<Session> Session { get; } = new ReactiveProperty<Session>();
        public ReactiveProperty<string> PostMessage { get; } = new ReactiveProperty<string>(string.Empty);

        public ReadOnlyReactiveProperty<bool> IsLoggedIn { get; }

        public ReactiveCommand LoginCommand { get; }
        public ReactiveCommand PostCommand { get; }

        private readonly IBlueskyApi _bueskyApi;

        public MainWindowModel(IBlueskyApi blueskyApi)
        {
            _bueskyApi = blueskyApi;

            LoginCommand = UserName
                .CombineLatest(Password, (user, pass) =>
                {
                    return !(string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass));
                })
                .ToReactiveCommand();

            IsLoggedIn = Session
                .Any ((s) =>
                {
                    return s != null && !string.IsNullOrEmpty(s.Email);
                })
                .ToReadOnlyReactiveProperty<bool>();

            PostCommand = Session
                .CombineLatest(PostMessage, (s, p) =>
                {
                    return s != null && !string.IsNullOrEmpty(s.Email) && !string.IsNullOrEmpty(p);
                })
                .ToReactiveCommand();

            LoginCommand.Subscribe(LoginPressed);
            PostCommand.Subscribe(PostPressed);
        }

        private async void LoginPressed()
        {
            Login cmd = new Login(UserName.Value, Password.Value);
            var result = await _bueskyApi.Login(cmd, CancellationToken.None);
            result.Switch(
                session =>
                {
                    Session.Value = session;
                },
                error =>
                {
                    Debug.WriteLine(error.Detail.Message);
                });
        }

        private async void PostPressed()
        {
            CreatePost post = new CreatePost(PostMessage.Value);
            var posted = await _bueskyApi.CreatePost(post, CancellationToken.None);
            posted
                .Switch(
                res =>
                {
                    PostMessage.Value = string.Empty;
                },
                err =>
                {
                    Debug.WriteLine(err.Detail.Message);
                }
                );
        }
    }
}
