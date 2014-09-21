library Dispatcher;

import "dart:async";

class AppDispatcher {
  static final AppDispatcher _singleton = new AppDispatcher._internal();
  var actionStream = new StreamController.broadcast();

  factory AppDispatcher() {
    return _singleton;
  }

  AppDispatcher._internal();

  void attach(listener) {
    actionStream.stream.listen(listener);
  }

  void handleAction(payload){
    actionStream.add(payload);
  }
}