library Dispatcher;

import "dart:async";

class AppDispatcher {
  static final AppDispatcher _singleton = new AppDispatcher._internal();
  var actionStream = new StreamController();

  factory AppDispatcher() {
    return _singleton;
  }

  AppDispatcher._internal();

  void Attach(listener) {
    actionStream.stream.listen(listener);
  }

  void Dispatch(payload){
    print("Dispatching " + payload["actionType"]);
    actionStream.add(payload);
  }
}