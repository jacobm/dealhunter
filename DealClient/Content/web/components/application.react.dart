library Application;

import "package:react/react.dart" as react;
import "../dispatcher/app_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;
import "../components/search.react.dart";
import "../components/feed_watches.react.dart";
import "../components/navbar.react.dart";
import "package:route/client.dart";
import "package:route/url_pattern.dart";

class _Application extends react.Component {
  componentWillMount(){
    var router = new Router(useFragment: true)
        ..addHandler(new UrlPattern('/'), _showHome)
        ..addHandler(new UrlPattern('/search/term'), _showSearch)
        ..addHandler(new UrlPattern(r'(.*)'), _showCatchAll)
            ..listen();

    router.gotoPath("/#",  "Deal Home");
  }

  var homeComponent = react.div({},
      [react.div({"className": "row"},
                  [navBar({}),
                   search({"className" : "col-sm-8"}),
                   feedWatches({"className": "col-sm-4"})
                  ])
      ]);
  var searchComponent = react.div({}, "search");

  getInitialState(){
    return {"Component": homeComponent };
  }

  _showHome(String path){
    this.setState({"Component": homeComponent});
  }

  _showSearch(String path){
    this.setState({"Component": searchComponent});
  }

  _showCatchAll(String path) {
  }

  render() {
    var it = this.state["Component"];
    return it;

    return react.div({"className": "container"},
        [react.div({"className" : "row"},
                    [react.div({"className": "col-sm-8"}, "Find deals"),
                     react.div({"className": "col-sm-4"}, debugButton({}))]),
                     react.div({"className": "row"}, [
                        search({"className" : "col-sm-8"}),
                        feedWatches({"className": "col-sm-4"})])
                     ]);
  }
}
var application = react.registerComponent(() => new _Application());