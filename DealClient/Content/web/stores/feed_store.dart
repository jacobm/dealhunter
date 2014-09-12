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

  static final FeedStore _singleton = new FeedStore._internal();

  factory FeedStore() {
    return _singleton;
  }

  FeedStore._internal(){
    new AppDispatcher().attach(_onAction);
    eventDispatcher.attach(_onEvent);
  }

  List<SearchItem> get items => _searchItems;

  _search(String term){
    HttpRequest.getString("http://localhost:8888/search/" + term)
               .then((response) {
      var items = JSON.decode(response);
      _searchItems = items.map((x) => new SearchItem.fromMap(x)).toList();
      eventDispatcher.handleEvent(SearchResultReady);
    }).catchError((error){
      print(error);
    });
  }

  void _onEvent(event) {
    switch(event) {
      case AppEvents.UserLoggedInEvent:
        break;
    }
  }

  void _onAction(action){
    switch(action["actionType"]){
      case AppConstants.AppSearch:
        _search(action["payload"]["searchTerm"]);
        break;
    }
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