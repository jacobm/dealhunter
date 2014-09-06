library FeedStore;

import 'dart:async';
import 'dart:html';
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import "dart:convert";

class FeedStore {
  static const SearchResultReady = "SeachResultReady";
  List<SearchItem> _searchItems = new List<SearchItem>();

  static final FeedStore _singleton = new FeedStore._internal();
    var events = new StreamController<String>();

  factory FeedStore() {
    return _singleton;
  }

  FeedStore._internal(){
    new AppDispatcher().attach(_onAction);
  }

  List<SearchItem> get items => _searchItems;

  void Attach(listener){
    events.stream.listen(listener);
  }

  _search(String term){
    HttpRequest.getString("http://localhost:8888/search/" + term)
               .then((response) {
      var items = JSON.decode(response);
      _searchItems = items.map((x) => new SearchItem.fromMap(x)).toList();
      events.add(SearchResultReady);
    }).catchError((error){
      print(error);
    });
  }

  void _onAction(action){
    print(action);
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