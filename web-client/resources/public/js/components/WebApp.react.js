/** @jsx React.DOM */ 

var React = require('react');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var AppActions = require('../actions/AppActions');
var NavigationBar = require('./NavigationBar.react');
var SearchTextInput = require('./SearchTextInput.react');
var SearchResultTable = require('./SearchResultTable.react');
var PreviousSearches = require('./PreviousSearches.react');

var App = React.createClass({

    onSubmit: function(value) {
	AppActions.search(value);
    },


    render: function() {
	return (
	    <div>
		<NavigationBar />
                <div className="col-sm-6 col-md-6">
	           <SearchTextInput onSubmit={this.onSubmit} />
	        </div>

                <PreviousSearches />

		<SearchResultTable />
	    </div>
		
	);
    }
});


module.exports = App;
