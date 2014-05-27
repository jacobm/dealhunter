/** @jsx React.DOM */ 

var React = require('react');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var AppActions = require('../actions/AppActions');
var SearchTextInput = require('./SearchTextInput.react');
var SearchResultTable = require('./SearchResultTable.react');

var App = React.createClass({
    onSubmit: function(value) {
	AppActions.search(value);
    },

    render: function() {
	return (
	    <div>
		<SearchTextInput className="dingo" onSubmit={this.onSubmit} />
		<SearchResultTable />
	       Dingos
	    </div>
		
	);
    }
});


module.exports = App;
