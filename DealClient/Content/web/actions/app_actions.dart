library Actions;

import "../dispatcher/app_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;

var dispatcher = new AppDispatcher();

search(String text) {
  dispatcher.handleAction({
    "actionType": AppConstants.AppSearch,
    "payload": {"searchTerm": text}
  });
}

login(String token){
  dispatcher.handleAction({
    "actionType": AppConstants.LoginUser,
    "payload": {"token": token}
  });
}