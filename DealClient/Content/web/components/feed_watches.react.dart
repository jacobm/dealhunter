library FeedWatches;

import "package:react/react.dart" as react;
import "../stores/feed_store.dart";
import '../dispatcher/event_dispatcher.dart';
import '../constants/app_constants.dart' as AppConstants;

class _FeedWatches extends react.Component {
  FeedStore feedStore;
  EventDispatcher eventDispatcher = new EventDispatcher();
  UserFeedWatches _feedWatches;
  UserFeedWatches get UserState => _feedWatches;

  componentWillMount() {
     feedStore = new FeedStore();
     eventDispatcher.attach(_onFeedEvent);
  }

  void _onFeedEvent(Map event){
    switch(event["eventType"]){
      case AppConstants.UserStateUpdated:
        _feedWatches = feedStore.UserState;
        break;
    }
  }

  render() {
    return react.div({"className": this.props["className"]}, "FeedWatch");
  }
}

var feedWatches = react.registerComponent(() => new _FeedWatches());