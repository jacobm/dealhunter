library Items;

import 'package:intl/intl.dart';
import "package:react/react.dart" as react;
import "../stores/feed_store.dart";

class _SearchItem extends react.Component {

  SearchItem get item => this.props["item"];

  render() {
    var formatter = new DateFormat('dd-MM-yyyy');
    var date = formatter.format(item.postedAt);

    return react.li({"className": "row"},
        [react.img({"src": item.thumbnail, "className": "col-md-4"}),
         react.span({"className": "col-md-1"}, item.price.toString() + " kr"),
         react.span({"className": "col-md-1"}, date),
         react.span({"className": "col-md-4"}, item.location.postcode.toString() + " - " + item.location.city),
              react.span({"className": "col-md-6"},
             [item.text])]);
  }
}
var searchItem = react.registerComponent(() => new _SearchItem());

class _ItemList extends react.Component {

  searchTable(Searchresult result) {
    if (result == null){
      return react.ul({});
    }

    var res = result.items.map((x) => searchItem({"item": x}));
    return react.ul({}, res);
  }

  Searchresult get _searchResult => this.props["searchResult"];

  render() {
    return react.div({"className" : this.props["className"]},
        [searchTable(_searchResult)]);
  }
}
var itemList = react.registerComponent(() => new _ItemList());