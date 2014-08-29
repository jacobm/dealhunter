import "package:react/react.dart" as react;
import "package:react/react_client.dart";
import "dart:html";
import "components/seach_text_input.react.dart";
import "components/feed_watches.react.dart";
import "stores/feed_store.dart";
import "stores/user_store.dart";
import "dispatcher/app_dispatcher.dart";
import "constants/app_constants.dart" as AppConstants;

class _Item extends react.Component {
  componentWillReceiveProps(newProps) {
    print("Old props: $props");
    print("New props: $newProps");

  }

  shouldComponentUpdate(nextProps, nextState) {
    return false;
  }


  render() {
    return react.li({}, [props['text']]);
  }
}

var item = react.registerComponent(() => new _Item());

class _List extends react.Component {
  var items = ['item1', 'item2', 'item3'];

  remove() {
    items.removeAt(0);
    redraw();
  }

  render() {
    return react.div({}, [debugButton({}),
                          feedWatches({}),
                          userDisplay({}),
                          searchTextInput({})]);
  }
}
var list = react.registerComponent(() => new _List());

class _UserDisplay extends react.Component {

  getInitialState(){
    return {"user": null};
  }

  componentDidMount(domNode) {
    new UserStore().Attach(_onChange);
  }

  render() {
    if(_user != null)
    {
    return react.div({}, [_user.name + "  " + _user.imageUrl]);
    }
    else
    {
      return react.div({});
    }
  }

  CurrentUser get _user => this.state["user"];

  _onChange(String event){
    this.setState({"user": new UserStore().User});
  }
}
var userDisplay = react.registerComponent(() => new _UserDisplay());

class _DebugButton extends react.Component {

  _onClick(event){
    new AppDispatcher().Dispatch(
        {"actionType": AppConstants.LoginUser,
         "payload": "dingo"}
        );
  }

  render() {
    return react.button({"onClick": (e) => _onClick(e)}, "Debug");
  }
}
var debugButton = react.registerComponent(() => new _DebugButton());

void pp(s) {
  print("Received " + s);
}

void main() {
  var userStore = new UserStore();

  var f = new FeedStore();
  f.Poke("dingo");
  f.Poke("OMGZ");
  f.Attach(pp);

  setClientConfiguration();
  react.renderComponent(list({}), querySelector('#content'));
}