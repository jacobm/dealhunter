library FeedStore;

import 'dart:async';

class FeedStore {

    var s = new StreamController<String>();

    void Attach(listener){
      s.stream.listen(listener);
    }

    void Poke(String value){
      s.add(value);
    }
}