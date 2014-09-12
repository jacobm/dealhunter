library Search;

import 'package:intl/intl.dart';
import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/feed_store.dart";
import '../dispatcher/event_dispatcher.dart';
import '../actions/app_events.dart';

var ENTER_KEY_CODE = 13;

class _SearchTextInput extends react.Component {
  getInitialState() {
    var init = this.props['value'];
    if (init != null)
    {
      return {"value": init};
    }

    return {"value" : ""};
  }

  _onChange(event) {
    this.setState({"value": event.target.value});
  }

  _onKeyDown(event) {
    if (event.keyCode == ENTER_KEY_CODE) {
      _onSubmit();
    }
  }

  _onSubmit() {
    var value = this.state["value"];
    if (value != ""){
      this.props["onSubmit"](this.state["value"]);
      this.setState({"value": ""});
    }
  }

  render() {
    return
        react.div({"className": "row center-block"}, [
        react.div({"className": "col-sm-6 col-md-6"},
              react.input(
              {"onChange": (e) => _onChange(e),
               "onSubmit": (e) => _onSubmit(),
               "onKeyDown": (e) => _onKeyDown(e),
               "value" : _value,
               "placeholder": "Search",
               "className": "form-control"})),
        react.div({"className": "col-sm-3 col-md-3 input-group"},
             [react.button({"onClick": (e) => _onSubmit(),
                            "className": "btn btn-primary"},
                 react.i({"className": "glyphicon glyphicon-search"}))])]
    );
  }

  String get _value => this.state["value"];
}
var searchTextInput = react.registerComponent(() => new _SearchTextInput());

class _SearchItem extends react.Component {

  SearchItem get item => this.props["item"];

  render() {
    var formatter = new DateFormat('dd-MM-yyyy');
    var date = formatter.format(item.postedAt);

    return react.li({"className": "row"},
        [react.img({"src": item.thumbnail, "className": "col-md-4"}),
         react.span({"className": "col-md-1"}, item.price.toString() + " kr"),
         react.span({"className": "col-md-1"}, date),
         react.span({"className": "col-md-4"}, item.location.postcode.toString() + " - " + item.location.city),
              react.span({"className": "col-md-6"},
             [item.text])]);
  }
}
var searchItem = react.registerComponent(() => new _SearchItem());

class _Search extends react.Component {
  FeedStore feedStore = new FeedStore();
  EventDispatcher eventDispatcher = new EventDispatcher();

  getInitialState(){
    return {"searchResult": feedStore.items};
  }

  componentDidMount(domNode) {
    eventDispatcher.attach(_onChange);
    //_onSubmit("dingo");
  }

  searchTable(List<SearchItem> items) {
    var res = items.map((x) => searchItem({"item": x}));
    return react.ul({}, res);
  }

  render() {
    return react.div({"className" : this.props["className"]},
        [searchTextInput({"onSubmit": _onSubmit}),
         searchTable(_searchResults)]);
  }

  List<SearchItem> get _searchResults => this.state["searchResult"];
                   set _searchResults (List<SearchItem> value) =>
                       this.setState({"searchResult": value});

  _onSubmit(String text) {
    Actions.search(text);
  }

  _onChange(String change) {
    switch(change) {
      case SearchResultReady:
        _searchResults = feedStore.items;
        break;
    }
  }
}
var search = react.registerComponent(() => new _Search());