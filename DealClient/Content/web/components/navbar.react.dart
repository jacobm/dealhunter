library Navbar;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/user_store.dart";

class _GoogleLoginButton extends react.Component {

  _onClick(event) {
    Actions.login();
  }

  _onLogoutClick(event) {
    Actions.logout();
  }

  render (){
    return react.div({},
      [react.button({"onClick": _onClick}, "Login")]
    );
  }
}
var googleLoginButton = react.registerComponent(() => new _GoogleLoginButton());

class _CurrentUser extends react.Component {

  CurrentUser get User => this.props["user"];

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
}
var currentUser = react.registerComponent(() => new _CurrentUser());

class _NavBar extends react.Component {
  UserStore userStore;

  CurrentUser get User => this.state["User"];

  componentWillMount() {
    userStore = new UserStore();
    userStore.Attach(_onUserEvent);
  }

  getInitialState(){
    return {"User": null};
  }

  render() {
    var children = [];

    if (User == null){
      children = [googleLoginButton({})];
    } else {
      children = [currentUser({"user": User})];
    }

    return react.div({"className": "navbar navbar-default"},
        [react.div({"className": "navbar-brand"}, "Deals"),
         react.div({"className": "navbar-right"}, children)]);
  }

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
var navBar = react.registerComponent(() => new _NavBar());