import "package:react/react.dart" as react;
import "package:react/react_client.dart";
import "dart:html";
import "components/application.react.dart";
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


class _DebugButton extends react.Component {

  _onClick(event){
    new AppDispatcher().handleAction(
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

void showHome(String path) {
  print("home");
  // nothing to parse from path, since there are no groups
}

void showArticle(String path) {
  print("dingo");
  //var articleId = articleUrl.parse(path)[0];
  // show article page with loading indicator
  // load article from server, then render article
}

void matchPages(String Path) {
  print("match");
}

void main() {

  setClientConfiguration();
  react.renderComponent(application({}), querySelector('#content'));
}