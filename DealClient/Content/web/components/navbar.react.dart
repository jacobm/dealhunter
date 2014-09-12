library Navbar;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/user_store.dart";
import '../dispatcher/event_dispatcher.dart';
import '../actions/app_events.dart' as AppEvents;

class _GoogleLoginButton extends react.Component {

  _onClick(event) {
    Actions.login();
  }

  _onLogoutClick(event) {
    Actions.logout();
  }

  render (){
    return react.div({},
      [react.a({"onClick": _onClick,
                "className": "sign-in btn btn-block btn-social btn-google-plus"},
                [react.i({"className": "fa fa-google-plus"}),
                 "Sign in with Google"])]
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
      react.div({}, [react.button({"onClick": _onLogoutClick,
                                   "className": "sign-out"}, "Logout"),
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
  EventDispatcher eventDispatcher = new EventDispatcher();

  CurrentUser get User => this.state["User"];

  componentWillMount() {
    userStore = new UserStore();
    eventDispatcher.attach(_onUserEvent);
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
      case AppEvents.UserLoggedInEvent:
        this.setState({"User": userStore.User});
        break;
      case AppEvents.UserLoggedOutEvent:
         this.setState({"User": userStore.User});
         break;
      default:
        break;
    }
  }
}
var navBar = react.registerComponent(() => new _NavBar());