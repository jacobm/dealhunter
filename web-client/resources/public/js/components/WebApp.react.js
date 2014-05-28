/** @jsx React.DOM */ 

var React = require('react');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var AppActions = require('../actions/AppActions');
var NavigationBar = require('./NavigationBar.react');
var SearchTextInput = require('./SearchTextInput.react');
var SearchResultTable = require('./SearchResultTable.react');

var App = React.createClass({

    render: function() {
	return (
	    <div>
		<NavigationBar />
		<SearchResultTable />
	       Dingos
	    </div>
		
	);
    }
});


module.exports = App;
