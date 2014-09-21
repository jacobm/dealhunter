library UserStore;

import 'dart:async';
import "dart:html";
import "package:google_oauth2_client/google_oauth2_browser.dart";
import "package:google_plus_v1_api/plus_v1_api_browser.dart";
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import "package:google_plus_v1_api/plus_v1_api_browser.dart" as plusclient;
import '../dispatcher/event_dispatcher.dart';
import '../actions/app_events.dart' as AppEvents;
import "dart:convert";

class CurrentUser {
  String name;
  String imageUrl;

  CurrentUser(this.name, this.imageUrl);
}

class TermPosition {
  String term;
  String position;

  TermPosition(this.term, this.position);
}

class State {
  List<TermPosition> _positions = new List<TermPosition>();

  State(this._positions);

  factory State.fromString(String value){
    var json = JSON.decode(JSON.decode(value)); // fix escapes
    var positions = json["positions"].map((x){
      return new TermPosition(x["term"], x["position"]);
    }).toList();

    return new State(positions);
  }
}


class UserStore {
  static final UserStore _singleton = new UserStore._internal();
  CurrentUser _user = null;
  State _userState = null;
  GoogleOAuth2 auth = null;
  EventDispatcher eventDispatcher = new EventDispatcher();

  CurrentUser get User => _user;

  State get UserState => _userState;

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
    auth.login(immediate: false, onlyLoadToken : true).then((Token token){
       HttpRequest.request(
           "api/login",
           method: 'POST',
           requestHeaders:{'Content-Type': 'application/json;charset=utf-8'},
           sendData: JSON.encode({"code": code}))
       .then((HttpRequest response){
         if (response.status != 200){
            throw new Exception("Server login failed");
         }

         _userState = new State.fromString(response.responseText);
         AppEvents.PublishUserStateUpdatedEvent(code);
       });

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