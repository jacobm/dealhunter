library UserStore;

import "package:google_oauth2_client/google_oauth2_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_browser.dart";
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import "package:google_plus_v1_api/plus_v1_api_browser.dart" as plusclient;
import '../dispatcher/event_dispatcher.dart';
import '../actions/app_events.dart' as AppEvents;

class CurrentUser {
  String name;
  String imageUrl;

  CurrentUser(this.name, this.imageUrl);
}

class UserStore {
  static final UserStore _singleton = new UserStore._internal();
  CurrentUser _user = null;
  GoogleOAuth2 auth = null;
  EventDispatcher eventDispatcher = new EventDispatcher();

  CurrentUser get User => _user;

  factory UserStore() {
    return _singleton;
  }

  UserStore._internal(){
    new AppDispatcher().attach(_onAction);
    eventDispatcher.attach(_onEvent);
    auth = new GoogleOAuth2(
          AppConstants.googleClientId,
          ["openid", plusclient.Plus.PLUS_ME_SCOPE],
          tokenLoaded: _tokenLoaded);
  }

  void _onEvent(Map event){
  }

  void _onAction(action){
    var payload = action["payload"];
    switch(action["actionType"]){
      case AppConstants.LoginUser:
        var accessToken = payload["accessToken"];
        var code = payload["code"];
        _loginUser(accessToken, code);
        break;
      case AppConstants.LogoutUser:
        _logoutUser();
        break;
      default:
        break;
    }
  }

  _logoutUser() {
    auth.logout();
    _user = null;
    AppEvents.PublishUserLoggedOutEvent();
  }

  _loginUser(accessToken, code) {
    AppEvents.PublishGoogleCodeReceivedEvent(code);
    auth.login(immediate: false, onlyLoadToken : true).then((Token token){
       var plus = new Plus(auth);
       plus.key = AppConstants.googleClientId;
       plus.oauth_token = token.data;
       plus.people.get("me").then((person){
          _user = new CurrentUser(person.name.givenName, person.image.url);
          AppEvents.PublishUserLoggedInEvent();
       });
    });
  }


  _tokenLoaded(token){
  }
}