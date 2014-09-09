library Navbar;

import "package:google_oauth2_client/google_oauth2_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_browser.dart" as plusclient;
import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../constants/app_constants.dart" as AppConstants;
import "../stores/user_store.dart";

class _GoogleLoginButton extends react.Component {
  GoogleOAuth2 auth;

  oauthReady(Token token) {
    Actions.login(auth);
  }

  componentDidMount(e) {
    this.auth = new GoogleOAuth2(
        AppConstants.googleClientId,
        ["openid", "email", plusclient.Plus.PLUS_ME_SCOPE],
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
      [react.button({"onClick": _onClick}, "Login")]
    );
  }
}
var googleLoginButton = react.registerComponent(() => new _GoogleLoginButton());

class _CurrentUser extends react.Component {
  UserStore userStore;

  componentWillMount() {
    userStore = new UserStore();
    userStore.Attach(_onUserEvent);
  }

  getInitialState(){
    return {"User": null};
  }

  render() {
    if (User == null) {
      return react.div({});
    }

    return react.div({},
      react.div({}, [User.name,
                     react.button({"onClick": _onLogoutClick}, "Logout"),
                     react.img({"src": User.imageUrl})])
    );
  }

  _onLogoutClick(event) {
    Actions.logout();
  }

  CurrentUser get User => this.state["User"];

  _onUserEvent(event) {
    switch(event) {
      case UserStore.UserLoggedInEvent:
        this.setState({"User": userStore.User});
        break;
      case UserStore.UserLoggedOutEvent:
             this.setState({"User": userStore.User});
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