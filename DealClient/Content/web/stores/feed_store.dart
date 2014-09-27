library FeedStore;

import 'dart:html';
import 'dart:async';
import '../constants/app_constants.dart' as AppConstants;
import '../dispatcher/app_dispatcher.dart';
import '../dispatcher/event_dispatcher.dart';
import "dart:convert";
import '../actions/app_events.dart' as AppEvents;

// SearchResults => annoying warnings, seems like a bug in editor?
class Searchresult {
  String term;
  List<SearchItem> items;

  Searchresult(this.term, this.items);
}

class FeedStore {
  EventDispatcher eventDispatcher = new EventDispatcher();
  UserFeedWatches _userState = null;
  Searchresult _searchResult = null;

  static final FeedStore _singleton = new FeedStore._internal();

  factory FeedStore() {
    return _singleton;
  }

  FeedStore._internal(){
    new AppDispatcher().attach(_onAction);
    eventDispatcher.attach(_onEvent);
  }

  UserFeedWatches get FeedWatches => _userState;

  Searchresult get SearchResult => _searchResult;

  _search(String term){
    HttpRequest.getString("http://localhost:8888/search/" + term)
               .then((response) {
      var items = JSON.decode(response);
      _searchResult = new Searchresult(
          term,
          items.map((x) => new SearchItem.fromMap(x)).toList());
      AppEvents.PublishSearchResultReady();
    }).catchError((error){
      print(error);
    });
  }

  void _onEvent(Map event) {
    switch(event["eventType"]) {
      case AppConstants.GoogleCodeReceivedEvent:
        var code = event["payload"]["code"];
        Storage.login(code).then((watches){
          _userState = watches;
          AppEvents.PublishUserStateUpdatedEvent();
        });
        break;
    }
  }

  void _onAction(action){
    switch(action["actionType"]){
      case AppConstants.AppSearch:
        _search(action["payload"]["searchTerm"]);
        break;
      case AppConstants.AddToWatches:
        var term = (action["payload"]["term"]);
        FeedWatches._positions.add(new TermPosition.fromTerm(term));
        Storage.save(FeedWatches).then((value){
          AppEvents.PublishUserStateUpdatedEvent();
        });
        break;
    }
  }
}

class Storage
{
  static Future<UserFeedWatches> login(String code){
    return HttpRequest.request(
        "api/login",
        method: 'POST',
        requestHeaders:{'Content-Type': 'application/json;charset=utf-8'},
        sendData: JSON.encode({"code": code}))
    .then((HttpRequest response){
      if (response.status == 200){
        return new UserFeedWatches.fromString(response.responseText);
      }
      throw new Exception("Server login failed");
    });
  }

  static Future<bool> save(UserFeedWatches watches){
    var data = JSON.encode(watches);
    return HttpRequest.request(
        "api/userdata",
        method: 'POST',
        requestHeaders:{'Content-Type': 'application/json;charset=utf-8'},
        sendData: data)
    .then((HttpRequest response){
      if (response.status == 200){
        return true;
      }
      return false;
    });
  }
}


class TermPosition {
  String term;
  String position;

  TermPosition(this.term, this.position);

  factory TermPosition.fromTerm(String term){
    return new TermPosition(term, null);
  }

  Map toJSon(){
    return {"term": term, "position": position};
  }
}

class UserFeedWatches {
  List<TermPosition> _positions = new List<TermPosition>();

  List<TermPosition> get Positions => _positions;

  UserFeedWatches(this._positions);

  factory UserFeedWatches.fromString(String value){
    var json = JSON.decode(JSON.decode(value)); // fix escapes
    var positions = json["positions"].map((x){
      return new TermPosition(x["term"], x["position"]);
    }).toList();

    return new UserFeedWatches(positions);
  }

  Map toJson(){
    var positions = Positions.map((x) => x.toJSon()).toList();
    return {"positions": positions};
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