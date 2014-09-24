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

login(String accessToken, String code){
  dispatcher.handleAction({
    "actionType": AppConstants.LoginUser,
    "payload": {"accessToken": accessToken,
                "code": code}
  });
}

logout(){
  dispatcher.handleAction({
    "actionType": AppConstants.LogoutUser,
    "payload": {}
  });
}

addToWatches(String term){
  dispatcher.handleAction({
    "actionType": AppConstants.AddToWatches,
    "payload": {"term": term}
  });
}