library SearchTextInput;

import "package:react/react.dart" as react;

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

  render() {
      return react.input(
          {"onChange": (e) => _onChange(e),
           "value" : this.state["value"],
           "placeholder": "Search"},
           react.div({}, this.state["value"])
      );
  }
}

var searchTextInput = react.registerComponent(() => new _SearchTextInput());