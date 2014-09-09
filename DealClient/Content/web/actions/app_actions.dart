library Actions;

import "package:google_oauth2_client/google_oauth2_browser.dart";
import "../dispatcher/app_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;

var dispatcher = new AppDispatcher();

search(String text) {
  dispatcher.handleAction({
    "actionType": AppConstants.AppSearch,
    "payload": {"searchTerm": text}
  });
}

login(GoogleOAuth2 auth){
  dispatcher.handleAction({
    "actionType": AppConstants.LoginUser,
    "payload": {"auth": auth}
  });
}

logout(){
  dispatcher.handleAction({
    "actionType": AppConstants.LogoutUser,
    "payload": {}
  });
}