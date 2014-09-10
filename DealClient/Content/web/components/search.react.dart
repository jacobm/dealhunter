library Search;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/feed_store.dart";

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
    return react.input(
        {"onChange": (e) => _onChange(e),
         "onSubmit": (e) => _onSubmit(),
         "onKeyDown": (e) => _onKeyDown(e),
         "value" : _value,
         "placeholder": "Search"},
         react.div({}, [_value,
                        react.button({
                         "onClick": (e) => _onSubmit()}, "Search")])
    );
  }

  String get _value => this.state["value"];
}
var searchTextInput = react.registerComponent(() => new _SearchTextInput());

class _SearchItem extends react.Component {

  SearchItem get item => this.props["item"];

  render() {
    return react.li({}, [item.text,
                         react.img({"src": item.thumbnail}),
                         react.span({}, [item.location.city,
                                         item.location.postcode,
                                         item.postedAt.toString(),
                                         item.price
                                         ])]);
  }
}
var searchItem = react.registerComponent(() => new _SearchItem());

class _Search extends react.Component {
  FeedStore feedStore = new FeedStore();

  getInitialState(){
    return {"searchResult": feedStore.items};
  }

  componentDidMount(domNode) {
    feedStore.Attach(_onChange);
  }

  searchTable(List<SearchItem> items) {
    var res = items.map((x) => searchItem({"item": x}));
    return react.ul({}, res);
  }

  render() {
    return react.div({"className" : "debug-border " + this.props["className"]},
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
      case FeedStore.SearchResultReady:
        _searchResults = feedStore.items;
        break;
    }
  }
}
var search = react.registerComponent(() => new _Search());