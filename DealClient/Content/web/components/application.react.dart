library Application;

import "package:react/react.dart" as react;
import "../dispatcher/app_dispatcher.dart";
import "../dispatcher/event_dispatcher.dart";
import "../constants/app_constants.dart" as AppConstants;
import "../components/search.react.dart";
import "../components/item_list.react.dart";
import "../stores/user_store.dart";
import "../stores/feed_store.dart";
import "../components/feed_watches.react.dart";
import "../components/navbar.react.dart";
import "package:route/client.dart";
import "package:route/url_pattern.dart";

class _Application extends react.Component {
  static UserStore _userStore = new UserStore();
  static FeedStore _feedStore = new FeedStore();
  static EventDispatcher eventDispatcher = new EventDispatcher();

  componentWillMount(){
    var router = new Router(useFragment: true)
        ..addHandler(new UrlPattern('/'), _showHome)
        ..addHandler(new UrlPattern('/search/term'), _showSearch)
        ..addHandler(new UrlPattern(r'(.*)'), _showCatchAll)
            ..listen();

    router.gotoPath("/#",  "Deal Home");
  }

  componentDidMount(domNode) {
    eventDispatcher.attach(_onEvent);
  }

  getInitialState() {
      return {"searchResult" : null,
              "isLoggedIn": false
      };
  }

  _onEvent(Map event) {
    switch(event["eventType"]) {
      case AppConstants.SearchResultReady:
      case AppConstants.UserLoggedOutEvent:
      case AppConstants.UserLoggedInEvent:
        this.setState({"searchResult": _feedStore.SearchResult,
                       "isLoggedIn": _userStore.IsLoggedIn});
        break;
    }
  }

  _showHome(String path){}
  _showSearch(String path){}
  _showCatchAll(String path) {}

  render() {
    return react.div({},
          [react.div({"className": "row"},
                      [navBar({}),
                       searchBar({}),
                       itemList({"className" : "col-sm-8",
                               "searchResult": this.state["searchResult"]}),
                       feedWatches({"className": "col-sm-4"})
                      ])]);
  }
}
var application = react.registerComponent(() => new _Application());