library UserStore;

import 'dart:async';
import "package:google_oauth2_client/google_oauth2_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_client.dart";
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import "package:google_plus_v1_api/plus_v1_api_browser.dart" as plusclient;


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
  CurrentUser _user = null;
  GoogleOAuth2 auth = null;
  StreamController<String> eventsCtrl = new StreamController<String>();
  Stream<String> events = null;

  CurrentUser get User => _user;

  factory UserStore() {
    return _singleton;
  }

  UserStore._internal(){
    new AppDispatcher().attach(_onAction);
    events = eventsCtrl.stream.asBroadcastStream();
    auth = new GoogleOAuth2(
          AppConstants.googleClientId,
          ["openid", "email", plusclient.Plus.PLUS_ME_SCOPE],
          tokenLoaded: _tokenLoaded);
  }


  void Attach(listener){
    events.listen(listener);
  }

  void _onAction(action){
    var payload = action["payload"];
    switch(action["actionType"]){
      case AppConstants.LoginUser:
        _loginUser();
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
    eventsCtrl.add(UserLoggedOutEvent);
  }

  _loginUser() {
    auth.login(immediate: true, onlyLoadToken : true).then((Token token){
      var plus = new Plus(auth);
       plus.key = AppConstants.googleClientId;
       plus.oauth_token = token.data;
       return plus.people.get("me");
    }).then((Person person){
      _user = new CurrentUser(person.name.givenName, person.image.url);
      eventsCtrl.add(UserLoggedInEvent);
    });
  }

  _tokenLoaded(token){
    _loginUser();
  }
}