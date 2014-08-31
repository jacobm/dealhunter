library UserStore;

import 'dart:async';
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
  static final UserLoggedInEvent = "UserLoggedInEvent";
  static final UserStore _singleton = new UserStore._internal();
  var _user = new CurrentUser("Jacob", "http://dingo.com/img/123");
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

  void _onAction(payload){
    print(payload);
    switch(payload["actionType"]){
      case AppConstants.LoginUser:
        print("logging in user");
        events.add(UserLoggedInEvent);
        break;
    }
  }
}