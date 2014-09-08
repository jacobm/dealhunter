library Navbar;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/user_store.dart";
import "package:google_oauth2_client/google_oauth2_browser.dart";




class _GoogleLoginButton extends react.Component {
  GoogleOAuth2 auth;

  oauthReady(Token token) {
    Actions.login(token.data);
  }

  componentDidMount(e) {
    this.auth = new GoogleOAuth2(
        "108491861456-fhikg55ecvi77bo9r6mfe7k9at7ebh0p.apps.googleusercontent.com",
        ["openid", "email"],
        tokenLoaded: oauthReady);
    auth.login(immediate: true, onlyLoadToken : true);
  }

  _onClick(event) {
    auth.login(immediate: false, onlyLoadToken : true);
  }

  _onLogoutClick(event) {
    auth.logout();
  }

  render (){
    return react.div({},
      [react.button({"onClick": _onClick}, "Login"),
       react.button({"onClick": _onLogoutClick}, "Logout"),]
    );
  }
}
var googleLoginButton = react.registerComponent(() => new _GoogleLoginButton());

class _CurrentUser extends react.Component {
  UserStore userStore;

  componentDidMount(event) {
    userStore = new UserStore();
    userStore.Attach(_onUserEvent);
  }

  getInitialState(){
    return {"User": null};
  }

  render() {
    return react.div({}, "USER");
  }

  _onUserEvent(event) {
    print(event);
    switch(event) {
      case UserStore.UserLoggedInEvent:
        print("Got user logged in event");
        break;
      default:
        break;
    }
  }
}
var currentUser = react.registerComponent(() => new _CurrentUser());

class _NavBar extends react.Component {
  render() {
    return react.div({}, ["Navbar",
                          googleLoginButton({}),
                          currentUser({})]);
  }
}
var navBar = react.registerComponent(() => new _NavBar());