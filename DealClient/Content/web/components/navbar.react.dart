library Navbar;

import "dart:js" as js;
import "dart:html" as html;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/user_store.dart";
import '../dispatcher/event_dispatcher.dart';
import '../constants/app_constants.dart' as AppConstants;

class _GoogleLoginButton extends react.Component {

  componentDidMount(data){
    var google = "(function(){var po = document.createElement('script');po.type = 'text/javascript';po.async = true;po.src = 'https://apis.google.com/js/client:plusone.js';var s = document.getElementsByTagName('script')[0];s.parentNode.insertBefore(po, s);})()";
    inject(google);
    js.context['signInCallback'] = signInCallback;
  }

  signInCallback(data){
    var token = data["access_token"];
    var code = data["code"];
    if (code != null){
      Actions.login(token, code);
    } else {
      print("login failed");
    }
  }

  void inject(String javascript){
    html.ScriptElement s = html.window.document.createElement('script');
    s.type ="text/javascript";
    s.text = javascript;
    html.document.body.nodes.add(s);
  }

  render (){
    return react.div({"id": "signinButton"},
        react.span({
            "className": "g-signin",
            "data-scope": "https://www.googleapis.com/auth/plus.login",
            "data-clientid": AppConstants.googleClientId,
            "data-redirecturi": "postmessage",
            "data-accesstype": "online",
            "data-cookiepolicy": "single_host_origin",
            "data-callback": "signInCallback"})
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

  _onUserEvent(Map event) {
    switch(event["eventType"]) {
      case AppConstants.UserLoggedInEvent:
        this.setState({"User": userStore.User});
        break;
      case AppConstants.UserLoggedOutEvent:
         this.setState({"User": userStore.User});
         break;
      default:
        break;
    }
  }
}
var navBar = react.registerComponent(() => new _NavBar());