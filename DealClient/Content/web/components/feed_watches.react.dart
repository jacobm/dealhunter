library FeedWatches;

import "package:react/react.dart" as react;
import "../actions/app_actions.dart" as Actions;
import "../stores/feed_store.dart";
import '../dispatcher/event_dispatcher.dart';
import '../actions/app_events.dart' as AppEvents;
import '../constants/app_constants.dart' as AppConstants;

class _FeedWatches extends react.Component {
  FeedStore feedStore;
  EventDispatcher eventDispatcher = new EventDispatcher();

  componentWillMount() {
     feedStore = new FeedStore();
     eventDispatcher.attach(_onFeedEvent);
  }

  void _onFeedEvent(Map event){

  }

  render() {
    return react.div({"className": this.props["className"]}, "FeedWatch");
  }
}

var feedWatches = react.registerComponent(() => new _FeedWatches());