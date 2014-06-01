/** @jsx React.DOM */ 

var React = require('react');
var _ = require('underscore');
var AppActions = require('../actions/AppActions');
var SearchStore = require('../stores/SearchStore');

function getSearchResult() {
    return {result: SearchStore.getSearchResult()};
}

var SearchResultTable = React.createClass({
 
    getInitialState: function() {
	return getSearchResult();
    },
   
    componentDidMount: function() {
	SearchStore.addChangeListener(this._onChange);
    },

    componentWillUnmount: function() {
	SearchStore.removeChangeListener(this._onChange);
    },
    
    render: function() {

	var rows = _.map(this.state.result, function(x) {
	    return (<li>
		    {x.text} {x.price} <img src={x.thumbnail} />
		    </li>
		   );
	});

	return (
	    <div>
		 {this.state.result.length} <ul>{rows}</ul>
	    </div>
	);
    },
    
    _onChange: function() {
	this.setState(getSearchResult());
    }
});

module.exports = SearchResultTable;
