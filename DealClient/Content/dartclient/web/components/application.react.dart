library Application;

import "package:react/react.dart" as react;
import "../dispatcher/app_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;
import "../components/search.react.dart";

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

class _Application extends react.Component {

  render() {
    return react.div({}, ["Application",
                          debugButton({}),
                          search({})
                          ]);
  }
}
var application = react.registerComponent(() => new _Application());