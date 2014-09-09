library UserStore;

import 'dart:async';
import "package:google_oauth2_client/google_oauth2_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_client.dart";
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';

class CurrentUser {
  String name;
  String imageUrl;

  CurrentUser(name, imageUrl){
    this.name = name;
    this.imageUrl = imageUrl;
  }
}


class UserStore {
  static const UserLoggedInEvent = "UserLoggedInEvent";
  static const UserLoggedOutEvent = "UserLoggedOutEvent";
  static final UserStore _singleton = new UserStore._internal();
  var _user = null;
  GoogleOAuth2 auth = null;
  var events = new StreamController<String>();

  factory UserStore() {
    return _singleton;
  }

  UserStore._internal(){
    new AppDispatcher().attach(_onAction);
  }


  void Attach(listener){
    events.stream.listen(listener);
  }

  CurrentUser get User => _user;

  void _onAction(action){
    var payload = action["payload"];
    switch(action["actionType"]){
      case AppConstants.LoginUser:
        auth = payload["auth"];
        _loginUser(auth).then((Person person){
          _user = new CurrentUser(person.name.givenName, person.image.url);
          events.add(UserLoggedInEvent);
        });
        break;
      case AppConstants.LogoutUser:
        auth.logout();
        _user = null;
        events.add(UserLoggedOutEvent);
        break;
      default:
        break;
    }
  }

  _loginUser(GoogleOAuth2 auth) {
    var plus = new Plus(auth);
    plus.key = AppConstants.googleClientId;
    plus.oauth_token = auth.token.data;
    return plus.people.get("me");
  }
}