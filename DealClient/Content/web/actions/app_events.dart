library Events;

import "../dispatcher/event_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;

EventDispatcher dispatcher = new EventDispatcher();

PublishSearchResultReady(String searchTerm) {
  dispatcher.publishEvent({
    "eventType": AppConstants.SearchResultReady,
    "payload": {"searchTerm": searchTerm}
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

PublishUserStateUpdatedEvent(String code){
  dispatcher.publishEvent({
    "eventType": AppConstants.UserStateUpdated,
    "payload": {"code": code}
  });
}