library FeedWatches;

import "package:react/react.dart" as react;
import "../stores/feed_store.dart";

class _FeedWatches extends react.Component {

  UserFeedWatches get _feedWatches => this.props["feedWatches"];

  renderWatches(){
    if (this._feedWatches == null){
      return react.div({});
    }

    return this._feedWatches.Positions.map((x){
      return react.li({}, x.term);
    });
  }

  render() {
    return react.div({"className": this.props["className"]},
        react.div({}, [
            "FeedWatch",
            react.ul({}, renderWatches())
            ]));
  }
}

var feedWatches = react.registerComponent(() => new _FeedWatches());