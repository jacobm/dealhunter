/** @jsx React.DOM */ 

var React = require('react');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var AppActions = require('../actions/AppActions');

var PreviousSearchEntry = React.createClass({

    render: function() {
        return (
            <div>
                 {this.props.searchTerm} {this.props.newItems.length} 
            </div>
        );
    }
});

module.exports = PreviousSearchEntry;
