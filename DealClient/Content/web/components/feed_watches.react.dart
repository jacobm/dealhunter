library FeedWatches;

import "package:react/react.dart" as react;

class _FeedWatches extends react.Component {

  render() {
    return react.div({}, "FeedWatch");
  }
}

var feedWatches = react.registerComponent(() => new _FeedWatches());