library FeedStore;

import 'dart:html';
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import '../dispatcher/event_dispatcher.dart';
import "dart:convert";
import '../actions/app_events.dart' as AppEvents;

class FeedStore {
  List<SearchItem> _searchItems = new List<SearchItem>();
  EventDispatcher eventDispatcher = new EventDispatcher();
  State _userState = null;

  static final FeedStore _singleton = new FeedStore._internal();

  factory FeedStore() {
    return _singleton;
  }

  FeedStore._internal(){
    new AppDispatcher().attach(_onAction);
    eventDispatcher.attach(_onEvent);
  }

  State get UserState => _userState;

  List<SearchItem> get items => _searchItems;

  _search(String term){
    HttpRequest.getString("http://localhost:8888/search/" + term)
               .then((response) {
      var items = JSON.decode(response);
      _searchItems = items.map((x) => new SearchItem.fromMap(x)).toList();
      AppEvents.PublishSearchResultReady(term);
    }).catchError((error){
      print(error);
    });
  }

  void _onEvent(Map event) {
    switch(event["eventType"]) {
      case AppConstants.GoogleCodeReceivedEvent:
        _onGoogleCodeReceived(event["payload"]["code"]);
        break;
    }
  }

  void _onGoogleCodeReceived(String code){
    HttpRequest.request(
        "api/login",
        method: 'POST',
        requestHeaders:{'Content-Type': 'application/json;charset=utf-8'},
        sendData: JSON.encode({"code": code}))
    .then((HttpRequest response){
      if (response.status != 200){
         throw new Exception("Server login failed");
      }

      _userState = new State.fromString(response.responseText);
      AppEvents.PublishUserStateUpdatedEvent();
    });

  }

  void _onAction(action){
    switch(action["actionType"]){
      case AppConstants.AppSearch:
        _search(action["payload"]["searchTerm"]);
        break;
    }
  }
}

class TermPosition {
  String term;
  String position;

  TermPosition(this.term, this.position);
}

class State {
  List<TermPosition> _positions = new List<TermPosition>();

  State(this._positions);

  factory State.fromString(String value){
    var json = JSON.decode(JSON.decode(value)); // fix escapes
    var positions = json["positions"].map((x){
      return new TermPosition(x["term"], x["position"]);
    }).toList();

    return new State(positions);
  }
}

class Location {
  int postcode;
  String city;

  Location(this.postcode, this.city);
}

class SearchItem {
  String text;
  String thumbnail;
  int price;
  String dbaLink;
  String dbaId;
  DateTime postedAt;
  Location location;

  SearchItem(){}

  factory SearchItem.fromMap(Map map) {
    var item = new SearchItem();
    item.text = map["text"];
    item.thumbnail = map["thumbnail"];
    item.price = map["price"];
    item.dbaLink = map["dbaLink"];
    item.dbaId = map["dbaId"];
    String dingo = map["postedAt"];
    item.postedAt = DateTime.parse(dingo);
    item.location = new Location(map["location"]["postcode"],
                                 map["location"]["city"]);

    return item;
  }
}