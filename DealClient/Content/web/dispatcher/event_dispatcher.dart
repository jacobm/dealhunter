library EventDispatcher;

import "dart:async";

class EventDispatcher {
  static final EventDispatcher _singleton = new EventDispatcher._internal();
  var eventCtrl = new StreamController.broadcast();

  factory EventDispatcher() {
    return _singleton;
  }

  EventDispatcher._internal();

  void attach(listener) {
    eventCtrl.stream.listen(listener);
  }

  void publishEvent(String eventName){
    eventCtrl.add(eventName);
  }
}