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
    return
        react.div({}, [
          react.div({"className": "col-sm-4 col-md-4"},
                react.input(
                {"onChange": (e) => _onChange(e),
                 "onSubmit": (e) => _onSubmit(),
                 "onKeyDown": (e) => _onKeyDown(e),
                 "value" : _value,
                 "placeholder": "Search",
                 "className": "form-control"})),
          react.div({"className": "col-sm-4 col-md-4 input-group"},
               [react.button({"onClick": (e) => _onSubmit(),
                              "className": "btn btn-primary"},
                   react.i({"className": "glyphicon glyphicon-search"}))])]
    );
  }

  String get _value => this.state["value"];
}
var searchTextInput = react.registerComponent(() => new _SearchTextInput());

class _AddToWatchesButton extends react.Component {

  render() {
    return
        react.div({"className": "col-sm-4 col-md-4"},
            [react.button({"className": "btn btn-primary"}, "Watch")]);
  }
}
var addToWatchesButton = react.registerComponent(() => new _AddToWatchesButton());

class _SearchBar extends react.Component {
  render() {
    return react.div({"className": "row center-block"},
        [searchTextInput({"onSubmit": _onSubmit}),
         addToWatchesButton({})]);
  }

  bool get _isLoggedIn => this.props["isLoggedIn"];
  Searchresult get _searchResult => this.props["searchResult"];
  UserFeedWatches get _feedWatches => this.props["userState"];

  _onSubmit(String text) {
     Actions.search(text);
  }
}
var searchBar = react.registerComponent(() => new _SearchBar());