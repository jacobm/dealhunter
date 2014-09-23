library Events;

import "../dispatcher/event_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;

EventDispatcher dispatcher = new EventDispatcher();

PublishSearchResultReady() {
  dispatcher.publishEvent({
    "eventType": AppConstants.SearchResultReady,
  });
}
PublishUserLoggedInEvent() {
  dispatcher.publishEvent({
    "eventType": AppConstants.UserLoggedInEvent,
    "payload": {}
  });
}
PublishUserLoggedOutEvent() {
  dispatcher.publishEvent({
    "eventType": AppConstants.UserLoggedOutEvent,
    "payload": {}
  });
}

PublishGoogleCodeReceivedEvent(String code){
  dispatcher.publishEvent({
    "eventType": AppConstants.GoogleCodeReceivedEvent,
    "payload": {"code": code}
  });
}

PublishUserStateUpdatedEvent(){
  dispatcher.publishEvent({
    "eventType": AppConstants.UserStateUpdated,
    "payload": {}
  });
}