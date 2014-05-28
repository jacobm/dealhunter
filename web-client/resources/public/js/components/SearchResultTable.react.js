/** @jsx React.DOM */ 

var React = require('react');
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
	SearchStore.removeChangeListener(this_onChange);
    },
    
    render: function() {
	return (
		<div>
		SearchTableResult {this.state.result.length}
	    </div>
	);
    },
    
    _onChange: function() {
	this.setState(getSearchResult());
    }
});

module.exports = SearchResultTable;
